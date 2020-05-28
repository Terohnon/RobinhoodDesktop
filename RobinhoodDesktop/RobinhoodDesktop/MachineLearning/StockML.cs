using System;
using System.Collections.Generic;
using NumSharp;
using System.Diagnostics;
using System.Text;
using System.Linq;
using Tensorflow;
using static Tensorflow.Binding;

namespace RobinhoodDesktop.MachineLearning
{
    public class StockML
    {
        #region Variables
        /// <summary>
        /// Should be populated with the input data to train on
        /// </summary>
        public NDArray TrainFeatures = null;

        /// <summary>
        /// Should be populated with the expected outputs for the given inputs
        /// to be used during training
        /// </summary>
        public NDArray TrainLabels = null;

        /// <summary>
        /// Should be populated with the input data to test on
        /// </summary>
        public NDArray TestFeatures = null;

        /// <summary>
        /// Should be populated with the expected outputs for the given inputs
        /// to be used during testing
        /// </summary>
        public NDArray TestLabels = null;

        /// <summary>
        /// The input layer
        /// </summary>
        protected Tensor Input = null;

        /// <summary>
        /// The labeled examples layer
        /// </summary>
        protected Tensor YTrue = null;

        /// <summary>
        /// The tensor that generates a prediction from the model
        /// </summary>
        protected Tensor Prediction = null;

        /// <summary>
        /// Indicates how well the model matched the expected outputs
        /// </summary>
        protected Tensor Accuracy = null;
        
        /// <summary>
        /// The loss function used to train the model
        /// </summary>
        protected Tensor LossFunc = null;

        /// <summary>
        /// The training operation (the chosen optimizer method)
        /// </summary>
        protected Operation TrainOp = null;

        /// <summary>
        /// The TensorFlow session
        /// </summary>
        protected Session Sess;
        #endregion

        /// <summary>
        /// Default method to build a fully-connected graph of the specified structure
        /// </summary>
        /// <param name="layerNodes">The number of nodes to have at each layer. Index [0] is the number of inputs, and [lenth - 1] is the number of outputs.</param>
        /// <returns></returns>
        public virtual Graph BuildFullyConnectedGraphInt(int[] layerNodes, float learningRate = 0.01f)
        {
            tf.enable_eager_execution();
            var g = new Graph().as_default();

            tf_with(tf.name_scope("Input"), delegate
            {
                Input = tf.placeholder(tf.float32, shape: new TensorShape(-1, layerNodes[0]));
                YTrue = tf.placeholder(tf.int32, shape: new TensorShape(-1, layerNodes[layerNodes.Length - 1]));
            });

            tf_with(tf.variable_scope("FullyConnected"), delegate
            {
                Tensor x = Input;
                Tensor y = null;

                for(int i = 1; i < (layerNodes.Length - 1); i++)
                {
                    var w = tf.get_variable("w" + i, shape: new TensorShape(layerNodes[i - 1], layerNodes[i]), initializer: tf.random_normal_initializer(stddev: 0.1f));
                    var b = tf.get_variable("b" + i, shape: new TensorShape(layerNodes[i]), initializer: tf.constant_initializer(0.1));
                    Prediction = tf.matmul(x, w) + b;
                    y = tf.nn.relu(Prediction);
                    x = y;
                }

                var w2 = tf.get_variable("w_out", shape: new TensorShape(layerNodes[layerNodes.Length - 2], layerNodes[layerNodes.Length - 1]), initializer: tf.random_normal_initializer(stddev: 0.1f));
                var b2 = tf.get_variable("b_out", shape: new TensorShape(layerNodes[layerNodes.Length - 1]), initializer: tf.constant_initializer(0.1));
                Prediction = tf.matmul(y, w2) + b2;
            });

            tf_with(tf.variable_scope("Loss"), delegate
            {
                var losses = tf.nn.sigmoid_cross_entropy_with_logits(tf.cast(YTrue, tf.float32), Prediction);
                LossFunc = tf.reduce_mean(losses);
            });

            tf_with(tf.variable_scope("Accuracy"), delegate
            {
                var y_pred = tf.cast(Prediction > 0, tf.int32);
                Accuracy = tf.reduce_mean(tf.cast(tf.equal(y_pred, YTrue), tf.float32));
            });

            // We add the training operation, ...
            var adam = tf.train.AdamOptimizer(learningRate);   // Set the learning rate here
            TrainOp = adam.minimize(LossFunc, name: "train_op");

            // Create the new session
            var config = new ConfigProto
            {
                InterOpParallelismThreads = 1,
                IntraOpParallelismThreads = 1,
                GpuOptions = new GPUOptions {  },

                LogDevicePlacement = true
            };
            Sess = tf.Session(config);

            return g;
        }

        /// <summary>
        /// Prepares the feature and labeled output data to be used to train the model
        /// </summary>
        /// <param name="inputs">The feature input data</param>
        /// <param name="labeledOutputs">The expected outputs corresponding to the inputs</param>
        public virtual void PrepareData<T>(float[,] inputs, T[,] labeledOutputs, float trainTestSplit = 0.5f) where T : struct
        {
            var x = np.array(inputs, false);
            var y = np.array(labeledOutputs, false);

            /* Shuffle the two arrays in-place */
            var state = np.random.get_state();
            np.random.shuffle(x);
            np.random.set_state(state);
            np.random.shuffle(y);

            /* Create views into the shuffled data for the train and test datasets */
            int trainLen = (int)(inputs.GetLength(0) * trainTestSplit);
            TrainFeatures = np.array(x[new Slice(0, trainLen)], true);
            TrainLabels = np.array(y[new Slice(0, trainLen)], true);
            TestFeatures = np.array(x[new Slice(TrainFeatures.Shape[0] , inputs.GetLength(0))], true);
            TestLabels = np.array(y[new Slice(TrainLabels.Shape[0], labeledOutputs.GetLength(0))], true);
        }
        
        /// <summary>
        /// Performs the training operation
        /// </summary>
        public virtual void Train(int epochs = 5000)
        {
            var sw = new Stopwatch();
            sw.Start();

            // init variables
            Sess.run(tf.global_variables_initializer());

            // check the accuracy before training
            var accuracyResult = Sess.run(Accuracy, new FeedItem(Input, TrainFeatures), new FeedItem(YTrue, TrainLabels));
            print($"Pre-accuacy: {accuracyResult}");

            // training
            foreach(var i in range(epochs))
            {
                // by sampling some input data (fetching)
                var loss = Sess.run(new ValueTuple<ITensorOrOperation, ITensorOrOperation>(TrainOp, LossFunc), new FeedItem(Input, TrainFeatures), new FeedItem(YTrue, TrainLabels));

                // We regularly check the loss
                if(i % 500 == 0)
                {
                    accuracyResult = Sess.run(Accuracy, new FeedItem(Input, TrainFeatures), new FeedItem(YTrue, TrainLabels));
                    var predicitons = Sess.run(Prediction, new FeedItem(Input, TrainFeatures));
                    print($"iter:{i} - loss:{loss.Item2} accuracy: {accuracyResult}");
                }
            }

            // Finally, we check our final accuracy
            accuracyResult = Sess.run(Accuracy, new FeedItem(Input, TrainFeatures), new FeedItem(YTrue, TrainLabels));
            print($"Post-accuacy: {accuracyResult}");

            print($"Time taken: {sw.Elapsed.TotalSeconds}s");
        }

        public virtual void Test(float[,] inputs, int[,] labeledOutputs)
        {
            NDArray x_inputs = (NDArray)inputs;
            NDArray y_inputs = (NDArray)labeledOutputs;

            Test(x_inputs, y_inputs);
        }

        public virtual void Test(NDArray inputs, NDArray labeledOutputs)
        {
            // Check the accuracy of the data
            var accuracyResult = Sess.run(Accuracy, new FeedItem(Input, inputs), new FeedItem(YTrue, labeledOutputs));
            print($"Test-accuacy: {accuracyResult}");
        }

        public virtual void Predict(NDArray inputs, out NDArray predicitons)
        {
            // Examine the predictions
            predicitons = Sess.run(Prediction, new FeedItem(Input, inputs));
        }
    }
}
